import { PendingTome } from '../../models/collections.models';

export interface ReleaseSplit {
  // On sale already (or no date known): something the user can go and buy.
  released: PendingTome[];
  // Announced by Comic Vine with a store date still ahead, soonest first.
  upcoming: PendingTome[];
}

// Dates are compared as ISO "yyyy-MM-dd" strings, which sort like the dates they are.
export function splitByRelease(tomes: readonly PendingTome[], today: string): ReleaseSplit {
  const released: PendingTome[] = [];
  const upcoming: PendingTome[] = [];

  for (const tome of tomes) {
    if (tome.storeDate && tome.storeDate > today) {
      upcoming.push(tome);
    } else {
      released.push(tome);
    }
  }

  upcoming.sort((left, right) => left.storeDate!.localeCompare(right.storeDate!));

  return { released, upcoming };
}

// Today's date in the user's time zone, as "yyyy-MM-dd".
export function localToday(now = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');

  return `${now.getFullYear()}-${month}-${day}`;
}
