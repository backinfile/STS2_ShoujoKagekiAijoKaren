const fs = require('fs');
const path = require('path');
const playwrightPath = require.resolve('playwright', { paths: [
  __dirname, process.cwd(),
  path.join(require('os').homedir(), '.cache', 'codex-runtimes', 'codex-primary-runtime', 'dependencies', 'node'),
] });
const { chromium } = require(playwrightPath);

(async () => {
  const root = path.resolve(process.argv[2]);
  const browser = await chromium.launch({ headless: true, channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto('file:///' + path.join(root, 'report.html').replaceAll('\\', '/'));
  await page.waitForFunction(() => document.querySelector('#canvas').dataset.loaded === 'true');
  const results = { title: await page.title(), sections: await page.locator('main section').count(), sequences: [] };
  results.desktopOverflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth);
  await page.screenshot({ path: path.join(root, 'report-desktop.png') });
  await page.locator('#frames').scrollIntoViewIfNeeded();
  for (let i = 0; i < 3; i++) {
    await page.locator('#sequence').selectOption(String(i));
    await page.waitForFunction(() => document.querySelector('#canvas').dataset.frame === document.querySelector('#slider').value);
    const before = Number(await page.locator('#slider').inputValue());
    await page.locator('#next').click();
    const after = Number(await page.locator('#slider').inputValue());
    if (after !== before + 1) throw Error('Frame navigation failed');
    await page.locator('#slider').evaluate((element) => {
      element.value = Math.floor(Number(element.max) / 2);
      element.dispatchEvent(new Event('input', { bubbles: true }));
    });
    await page.waitForFunction(() => document.querySelector('#canvas').dataset.frame === document.querySelector('#slider').value);
    await page.locator('.viewer').screenshot({ path: path.join(root, `report-original-frames-${i}.png`) });
    await page.locator('#play').click();
    await page.waitForFunction(() => document.querySelector('#play').textContent === '从头播放' && document.querySelector('#slider').value === document.querySelector('#slider').max);
    results.sequences.push({ index: i, startFrame: before, navigation: true, playbackCompleted: true });
  }
  await page.locator('#background').click();
  results.backgroundToggle = await page.locator('#screen').evaluate(element => element.classList.contains('light'));
  const broken = await page.locator('a[href^="file:"]').evaluateAll(links => links.map(a => a.href));
  results.localLinks = broken.map(url => ({ url, exists: fs.existsSync(require('url').fileURLToPath(url)) }));
  if (results.localLinks.some(link => !link.exists)) throw Error('Broken local link');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.evaluate(() => scrollTo(0, 0));
  results.mobileOverflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth);
  await page.screenshot({ path: path.join(root, 'report-mobile.png') });
  results.errors = errors;
  if (errors.length || results.desktopOverflow || results.mobileOverflow) throw Error(JSON.stringify(results));
  fs.writeFileSync(path.join(root, 'report-qa.json'), JSON.stringify(results, null, 2));
  console.log(JSON.stringify(results));
  await browser.close();
})().catch(error => { console.error(error); process.exit(1); });
