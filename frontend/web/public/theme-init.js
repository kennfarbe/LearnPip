// Apply the saved choice before the first paint; storage is optional.
(() => {
  let preference = 'system';
  try {
    preference = localStorage.getItem('learnpip-theme') || 'system';
  } catch {}
  const dark =
    preference === 'dark' ||
    (preference !== 'light' && matchMedia('(prefers-color-scheme: dark)').matches);
  document.documentElement.dataset.theme = dark ? 'dark' : 'light';
})();
