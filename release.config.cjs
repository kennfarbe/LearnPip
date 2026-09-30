module.exports = {
  branches: ['main'],
  tagFormat: 'v${version}',
  plugins: [
    '@semantic-release/commit-analyzer',
    '@semantic-release/release-notes-generator',
    ['@semantic-release/changelog', { changelogFile: 'CHANGELOG.md' }],
    ['@semantic-release/exec', { prepareCmd: './scripts/package-release.sh ${nextRelease.version}' }],
    ['@semantic-release/git', {
      assets: ['CHANGELOG.md'],
      message: 'chore(release): ${nextRelease.version} [skip ci]'
    }],
    ['@semantic-release/github', { assets: ['dist/*.tar.gz', 'dist/*.sha256'] }]
  ]
};
