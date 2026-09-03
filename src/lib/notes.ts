import type { CollectionEntry } from 'astro:content';

type Note = CollectionEntry<'notes'>;

/**
 * Newest first, with `order` as the tiebreak.
 *
 * Both the home page and /notes/ render this collection, and they have
 * drifted apart before -- the notes index was hardcoded and ended up citing
 * a project that had been deleted. One sort, imported twice, is the fix that
 * survives someone editing only one of the two files.
 *
 * The tiebreak is not decoration. The original pubDate sort was removed
 * because all three notes carried the same date, so every comparison returned
 * 0 and the rendered order fell to whatever the content layer happened to
 * yield -- it changed between builds and failed the zero-tolerance visual
 * suite with no source change. Dates were never the problem; identical
 * placeholder dates were. `order` guarantees a total ordering even when two
 * notes genuinely share a day, which is likely once these are written in
 * batches.
 */
export function sortNotes(notes: Note[]): Note[] {
  return [...notes].sort((a, b) => {
    const byDate = b.data.pubDate.getTime() - a.data.pubDate.getTime();
    return byDate !== 0 ? byDate : a.data.order - b.data.order;
  });
}

/**
 * "23 May 2026".
 *
 * `timeZone: 'UTC'` is load-bearing. A bare `2026-05-23` in frontmatter parses
 * to UTC midnight, so formatting it in any negative-offset local zone (this
 * site is authored in US Central) renders the previous day. The build machine
 * must not be able to change what a date says.
 */
export function formatNoteDate(d: Date): string {
  return d.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  });
}
