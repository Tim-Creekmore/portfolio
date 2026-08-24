import { defineConfig } from 'astro/config';
import mdx from '@astrojs/mdx';
import sitemap from '@astrojs/sitemap';

export default defineConfig({
  site: 'https://www.timothycreekmore.com',
  integrations: [mdx(), sitemap()],
  output: 'static'
});
