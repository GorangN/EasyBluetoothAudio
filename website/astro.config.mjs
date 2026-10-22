import { defineConfig } from 'astro/config';
import react from '@astrojs/react';
import sitemap from '@astrojs/sitemap';

export default defineConfig({
  site: 'https://gorangn.github.io',
  base: '/EasyBluetoothAudio',
  output: 'static',
  trailingSlash: 'always',
  integrations: [react(), sitemap()],
});
