import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../../../core/http/api-error';
import { MangaCollectionDetail } from '../../models/collection.models';
import { CollectionApi } from '../../services/collection-api';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { OwnedVolumeRow } from '../../components/owned-volume-row/owned-volume-row';
import { MissingVolumesPanel } from '../../components/missing-volumes-panel/missing-volumes-panel';

@Component({
  selector: 'app-collection-detail-page',
  imports: [ReactiveFormsModule, RouterLink, AppAlert, OwnedVolumeRow, MissingVolumesPanel],
  templateUrl: './collection-detail-page.html',
  styleUrl: './collection-detail-page.scss'
})
export class CollectionDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly collectionApi = inject(CollectionApi);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly isUpdatingTotalVolumes = signal(false);
  readonly isLoading = signal(true);
  readonly isAddingVolume = signal(false);
  readonly removingVolumeNumber = signal<number | null>(null);

  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly item = signal<MangaCollectionDetail | null>(null);

  private collectionItemId: string | null = null;

  readonly addVolumeForm = this.formBuilder.group({
    volumeNumber: [1, [Validators.required, Validators.min(1)]],
    purchaseDate: [''],
    price: [null as number | null],
    store: ['']
  });

  readonly totalVolumesForm = this.formBuilder.group({
    customTotalVolumes: ['']
  });

  constructor() {
    this.route.paramMap.subscribe(params => {
      this.collectionItemId = params.get('id');

      if (!this.collectionItemId) {
        this.isLoading.set(false);
        this.errorMessage.set('El identificador de la colección no es válido.');
        return;
      }

      this.loadDetail();
    });

    
  }

  loadDetail(): void {
    if (!this.collectionItemId) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.item.set(null);

    this.collectionApi.getCollectionItem(this.collectionItemId).subscribe({
      next: response => {
        this.item.set(response.item);

        this.totalVolumesForm.patchValue({
          customTotalVolumes: response.item.effectiveTotalVolumes?.toString() ?? ''
        });

        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido cargar el detalle de la colección.')
        );

        this.isLoading.set(false);
      }
    });
  }

  addVolume(): void {
    if (!this.collectionItemId) {
      return;
    }

    if (this.addVolumeForm.invalid) {
      this.addVolumeForm.markAllAsTouched();
      return;
    }

    const formValue = this.addVolumeForm.getRawValue();

    this.isAddingVolume.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.collectionApi.addOwnedVolume(this.collectionItemId, {
      volumeNumber: formValue.volumeNumber,
      purchaseDate: formValue.purchaseDate || null,
      price: formValue.price,
      store: formValue.store || null
    }).subscribe({
      next: response => {
        this.successMessage.set(`Tomo ${response.volumeNumber} añadido correctamente.`);
        this.isAddingVolume.set(false);

        this.addVolumeForm.patchValue({
          volumeNumber: response.volumeNumber + 1,
          purchaseDate: '',
          price: null,
          store: ''
        });

        this.loadDetail();
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido añadir el tomo.')
        );

        this.isAddingVolume.set(false);
      }
    });
  }

  removeVolume(volumeNumber: number): void {
    if (!this.collectionItemId) {
      return;
    }

    const shouldRemove = confirm(
      `¿Seguro que quieres quitar el tomo ${volumeNumber}?`
    );

    if (!shouldRemove) {
      return;
    }

    this.removingVolumeNumber.set(volumeNumber);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.collectionApi.removeOwnedVolume(this.collectionItemId, volumeNumber).subscribe({
      next: response => {
        this.successMessage.set(response.message);
        this.removingVolumeNumber.set(null);
        this.loadDetail();
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido quitar el tomo.')
        );

        this.removingVolumeNumber.set(null);
      }
    });
  }

  updateTotalVolumes(): void {
    if (!this.collectionItemId) {
      return;
    }

    const controlValue = this.totalVolumesForm.controls.customTotalVolumes.value;

    const rawValue =
      controlValue === null || controlValue === undefined
        ? ''
        : String(controlValue).trim();

    const totalVolumes =
      rawValue === ''
        ? null
        : Number(rawValue);

    if (totalVolumes !== null && Number.isNaN(totalVolumes)) {
      this.errorMessage.set('El total de tomos debe ser un número válido.');
      return;
    }

    if (totalVolumes !== null && totalVolumes < 1) {
      this.errorMessage.set('El total de tomos debe ser mayor que 0.');
      return;
    }

    this.isUpdatingTotalVolumes.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.collectionApi.updateCustomTotalVolumes(this.collectionItemId, {
      totalVolumes
    }).subscribe({
      next: () => {
        this.successMessage.set('Total de tomos actualizado correctamente.');
        this.isUpdatingTotalVolumes.set(false);
        this.loadDetail();
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido actualizar el total de tomos.')
        );

        this.isUpdatingTotalVolumes.set(false);
      }
    });
  }
}