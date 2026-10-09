import { Pipe, PipeTransform } from '@angular/core';

// Comic Vine serves every cover in several renditions that differ only in one path
// segment: .../uploads/scale_medium/0/308/x.jpg, .../uploads/original/0/308/x.jpg...
// Covers here are shown at most ~200 px wide, so the 458x640 "scale_small" (65 KB) is
// sharp even on 2x screens, half of "scale_medium" and a tenth of "original". This
// also shrinks covers stored before the API started picking the small rendition.
const RENDITION = /\/uploads\/(original|scale_medium|scale_large|screen_kubrick|screen_medium)\//;

export function smallCover(url: string | null | undefined): string | null {
  return url ? url.replace(RENDITION, '/uploads/scale_small/') : null;
}

@Pipe({ name: 'cover' })
export class CoverPipe implements PipeTransform {
  transform(url: string | null | undefined): string | null {
    return smallCover(url);
  }
}
