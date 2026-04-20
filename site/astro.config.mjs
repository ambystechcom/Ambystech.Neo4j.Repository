// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

export default defineConfig({
  site: 'https://ambystechcom.github.io',
  base: '/Ambystech.Neo4j.Repository/',
  integrations: [
    starlight({
      title: 'Ambystech.Neo4j.Repository',
      description: 'Neo4j Repository pattern implementation for .NET.',
      logo: {
        src: './src/assets/icon.png',
      },
      components: {
        Footer: './src/components/Footer.astro',
      },
      social: [
        {
          icon: 'github',
          label: 'GitHub',
          href: 'https://github.com/ambystechcom/Ambystech.Neo4j.Repository',
        },
      ],
      sidebar: [
        {
          label: 'Introduction',
          link: '/',
        },
        {
          label: 'Getting Started',
          link: '/getting-started/',
        },
        {
          label: 'Guides',
          items: [
            { label: 'Configuring the Neo4j Driver', link: '/guides/driver-configuration/' },
          ],
        },
        {
          label: 'Packages',
          items: [
            { label: 'Contracts', link: '/packages/contracts/' },
            { label: 'Repository', link: '/packages/repository/' },
          ],
        },
        {
          label: 'Example',
          link: '/example/',
        },
        {
          label: 'Contributing',
          link: '/contributing/',
        },
      ],
    }),
  ],
});
