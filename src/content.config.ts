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
    // Explicit ordering, matching the projects collection. Replaces pubDate:
    // all three notes carried the same date, which made every date comparison
    // a no-op and left the sort order decided by whatever the content layer
    // happened to yield -- the home page reordered between builds and failed
    // the zero-tolerance visual suite with no source change. An integer the
    // author sets cannot go ambiguous, and these are evergreen pieces rather
    // than dated posts, so a publication date was never load-bearing.
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
