import { defineCollection, z } from 'astro:content';

const projects = defineCollection({
  type: 'content',
  schema: z.object({
    title: z.string(),
    summary: z.string(),
    status: z.enum(['featured', 'in-progress', 'lab']),
    tools: z.array(z.string()),
    metrics: z.array(z.string()).optional(),
    url: z.string(),
    repo: z.string().optional()
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
