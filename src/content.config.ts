import { defineCollection, z } from 'astro:content';

const projects = defineCollection({
  type: 'content',
  schema: z.object({
    title: z.string(),
    // 'employed' vs 'self-directed' is load-bearing: the spec's whole point is
    // that a portfolio shows what you'd build unasked, not just what you were
    // paid for. Rendering can label or group on this.
    kind: z.enum(['employed', 'self-directed']),
    org: z.string().optional(),
    period: z.string(),
    order: z.number(),
    tech: z.array(z.string()),
    link: z.object({ href: z.string(), label: z.string() }).optional()
  })
});

const notes = defineCollection({
  type: 'content',
  schema: z.object({
    title: z.string(),
    summary: z.string(),
    // When this note was published. Restored after being removed once: the
    // removal was right for three standalone evergreen essays, and wrong for
    // what this collection is now -- a running journal added to over time,
    // including updates to ongoing projects. A journal with no dates cannot
    // show that it is still being written, which is the main thing it is for,
    // and an "update" with no date is not an update.
    //
    // The original failure was three *identical* placeholder dates, which made
    // every comparison a no-op and left ordering to the content layer. Real
    // distinct dates plus the `order` tiebreak below fix that properly. See
    // src/lib/notes.ts for the sort both render sites share.
    pubDate: z.coerce.date(),
    // Tiebreak for notes that share a date, and the reason the sort is total
    // rather than merely usually-stable. Also still the manual ordering knob
    // if a note ever needs pinning out of date order.
    order: z.number(),
    // The short label shown above each note's title on /notes/ and the home
    // page. Explicit rather than derived from tags[0]: the labels do not
    // transform cleanly from the slugs ("ai-ml-experiments" displays as
    // "AI/Data"), and a derivation with a lookup table is just this field
    // with extra steps.
    category: z.string(),
    tags: z.array(z.string()).default([])
  })
});

export const collections = { projects, notes };
