import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { ImportComicVineVolumeResponse } from '../../../collections/models/collections.models';
import { CollectionsApi } from '../../../collections/services/collections-api';
import { CatalogSearchResult } from '../../models/catalog.models';
import { CatalogSearchPage } from './catalog-search-page';

describe('CatalogSearchPage adding series', () => {
  let imports: Map<string, Subject<ImportComicVineVolumeResponse>>;
  let page: CatalogSearchPage;

  const result = (id: number): CatalogSearchResult => ({
    comicVineVolumeId: id,
    name: `Serie ${id}`,
    publisherName: 'Planeta',
    countOfIssues: 10,
    imageUrl: null,
    startYear: 2020,
    deck: null,
    siteDetailUrl: null,
    apiDetailUrl: `https://comicvine.gamespot.com/api/volume/4050-${id}/`
  });

  const imported = (id: number, tomesPending: boolean): ImportComicVineVolumeResponse => ({
    editionId: `edition-${id}`,
    userCollectionId: `collection-${id}`,
    comicVineVolumeId: id,
    title: `Serie ${id}`,
    publisherName: 'Planeta',
    totalIssues: 10,
    importedTomes: tomesPending ? 0 : 10,
    isCompleted: !tomesPending,
    tomesPending
  });

  beforeEach(() => {
    imports = new Map();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        {
          provide: CollectionsApi,
          useValue: {
            getCollections: () => new Subject(),
            importComicVineVolume: ({ apiDetailUrl }: { apiDetailUrl: string }) => {
              const response = new Subject<ImportComicVineVolumeResponse>();
              imports.set(apiDetailUrl, response);
              return response;
            }
          }
        }
      ]
    });

    page = TestBed.createComponent(CatalogSearchPage).componentInstance;
  });

  it('adds several series at once, each with its own state', () => {
    const first = result(1);
    const second = result(2);

    page.add(first);
    page.add(second);

    expect(page.addStateFor(first)).toEqual({ status: 'adding' });
    expect(page.addStateFor(second)).toEqual({ status: 'adding' });

    imports.get(second.apiDetailUrl)!.next(imported(2, true));

    expect(page.collectionIdFor(second)).toBe('collection-2');
    expect(page.isDownloading(second)).toBe(true);
    expect(page.addStateFor(second)).toBeNull();
    expect(page.addStateFor(first)).toEqual({ status: 'adding' });
    expect(page.announcement()).toBe('«Serie 2» añadida a tu estantería.');
  });

  it('ignores a second click while the series is being added', () => {
    const item = result(1);

    page.add(item);
    const firstRequest = imports.get(item.apiDetailUrl);
    page.add(item);

    expect(imports.get(item.apiDetailUrl)).toBe(firstRequest);
  });

  it('keeps the error on its card and lets that card retry', () => {
    const item = result(1);

    page.add(item);
    imports.get(item.apiDetailUrl)!.error(new Error('boom'));

    expect(page.addErrorFor(item)).toBe('No se ha podido añadir.');
    expect(page.collectionIdFor(item)).toBeNull();

    page.add(item);
    expect(page.addStateFor(item)).toEqual({ status: 'adding' });
  });

  it('does not flag a series as downloading when it arrived complete', () => {
    const item = result(1);

    page.add(item);
    imports.get(item.apiDetailUrl)!.next(imported(1, false));

    expect(page.isDownloading(item)).toBe(false);
  });
});
