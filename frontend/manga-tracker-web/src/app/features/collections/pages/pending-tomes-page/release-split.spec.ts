import { PendingTome } from '../../models/collections.models';
import { localToday, splitByRelease } from './release-split';

describe('splitByRelease', () => {
  const tome = (tomeId: string, storeDate: string | null): PendingTome => ({
    collectionId: 'c',
    editionId: 'e',
    tomeId,
    seriesTitle: 'Serie',
    editionName: 'Serie',
    publisherName: null,
    issueNumber: tomeId,
    normalizedNumber: Number(tomeId),
    tomeTitle: null,
    imageUrl: null,
    coverDate: null,
    storeDate
  });

  it('keeps tomes already on sale, or without a date, as something to buy', () => {
    const { released, upcoming } = splitByRelease(
      [tome('1', '2026-01-10'), tome('2', null), tome('3', '2026-10-08')],
      '2026-10-08'
    );

    expect(released.map(item => item.tomeId)).toEqual(['1', '2', '3']);
    expect(upcoming).toEqual([]);
  });

  it('moves tomes with a future store date to upcoming, soonest first', () => {
    const { released, upcoming } = splitByRelease(
      [tome('5', '2027-03-01'), tome('4', '2026-11-15'), tome('3', '2026-01-10')],
      '2026-10-08'
    );

    expect(released.map(item => item.tomeId)).toEqual(['3']);
    expect(upcoming.map(item => item.tomeId)).toEqual(['4', '5']);
  });

  it('formats today in local time as yyyy-MM-dd', () => {
    expect(localToday(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05');
  });
});
