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
    pubDate: z.date(),
    tags: z.array(z.string()).default([])
  })
});

export const collections = { projects, notes };
