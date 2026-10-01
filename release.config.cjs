const conventional = /^(feat|fix|perf|refactor|build|ci|docs|style|test|chore|revert)(\([a-z0-9._/-]+\))?!?: /;

module.exports = {
  branches: ['main'],
  tagFormat: 'v${version}',
  plugins: [
    ['@semantic-release/commit-analyzer', {
      preset: 'angular',
      parserOpts: {
        mergePattern: /^Merge pull request #\d+ from .+$/,
        mergeCorrespondence: [],
        headerPattern: /^(?:Merge pull request #\d+ from .+\n\n)?(.*)$/,
        headerCorrespondence: ['header']
      }
    }],
    '@semantic-release/release-notes-generator',
    ['@semantic-release/changelog', { changelogFile: 'CHANGELOG.md' }],
    ['@semantic-release/git', {
      assets: ['CHANGELOG.md'],
      message: 'chore(release): ${nextRelease.version} [skip ci]'
    }],
    ['@semantic-release/github', {
      successComment: false,
      failComment: false,
      releasedLabels: false
    }]
  ]
};
