import { Pipe, PipeTransform } from '@angular/core';

// "1 tomo", "14 tomos", "? tomos" when Comic Vine does not know the count.
@Pipe({ name: 'tomes' })
export class TomesPipe implements PipeTransform {
  transform(count: number | null | undefined): string {
    if (count === null || count === undefined) {
      return '? tomos';
    }

    return count === 1 ? '1 tomo' : `${count} tomos`;
  }
}
