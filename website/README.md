# EasyBluetoothAudio website

The product landing page lives at <https://gorangn.github.io/EasyBluetoothAudio/>.

## Development

Use Node.js 24 and npm. From this directory:

```sh
npm ci
npm run dev
```

Open the local URL printed by Astro, including the `/EasyBluetoothAudio/` base path.

```sh
npm run check
npm run build
npm test
npm run preview
```

The production build is generated in `dist/`. The application is static Astro with React integration for Magic UI components; GitHub Pages needs no application server.

The Playwright suite starts a production preview automatically and checks responsive layouts, download destinations, keyboard interaction, accessibility with axe, asset paths, reduced motion, and the no-JavaScript fallback. Local Windows runs use Microsoft Edge. On other systems, install Chromium once with `npx playwright install chromium` before running the tests. Rebuild before testing source changes.

## Deployment

The repository uses **Settings → Pages → Build and deployment → Source: GitHub Actions**. The `github-pages` environment allows deployments from `main`.

Every push to `main` runs `.github/workflows/pages.yml`. It installs the committed lockfile with `npm ci`, checks Astro and TypeScript, builds the static website, installs Chromium, and runs the Playwright suite. Only a build that passes those checks is uploaded from `website/dist` and deployed as a GitHub Pages artifact. The workflow also supports manual runs from the Actions tab.

This approach makes the published output reproducible and prevents a failed build from replacing the live site. Official GitHub artifact and deployment actions publish the checked build directly; no generated output or deployment branch needs to be committed. Build permissions are read-only, deployment permissions are restricted to the deployment job, and the workflow serializes deployments without interrupting a running publish.

The actions are pinned to immutable commits, verified against these releases:

| Action | Release |
| --- | --- |
| `actions/checkout` | `v7.0.1` |
| `actions/setup-node` | `v7.1.0` |
| `actions/configure-pages` | `v6.0.0` |
| `actions/upload-pages-artifact` | `v5.0.0` |
| `actions/deploy-pages` | `v5.0.1` |

`astro.config.mjs` sets static output, `site: 'https://gorangn.github.io'`, and `base: '/EasyBluetoothAudio'`. Public asset URLs must include that base, typically through `import.meta.env.BASE_URL`. Keep the case of the repository name unchanged. The canonical page URL includes the trailing slash.

To inspect a deployment:

```sh
gh run list --workflow pages.yml
gh api repos/GorangN/EasyBluetoothAudio/pages
```

If Pages is ever reset to branch publishing, switch its source back to **GitHub Actions**, then run the website workflow again. No secret or personal access token is required by the workflow.

References: [Astro's GitHub Pages guide](https://docs.astro.build/en/guides/deploy/github/) and [GitHub's custom Pages workflow documentation](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages).
