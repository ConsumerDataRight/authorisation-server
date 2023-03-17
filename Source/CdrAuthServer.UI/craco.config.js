const CracoAlias = require('craco-alias');
const webpack = require('webpack');

module.exports = {
  plugins: [
    {
      plugin: CracoAlias,
      options: {
        source: 'tsconfig',
        /* tsConfigPath should point to the file where "paths" are specified */
        tsConfigPath: './tsconfig.paths.json',
      },
    },
  ],
  webpack: {
    alias: {
      '@mui/styled-engine': '@mui/styled-engine-sc',
    },
    configure: (config) => {
      const fallback = config.resolve.fallback || {};
      Object.assign(fallback, {
        "crypto": require.resolve("crypto-browserify"),
        "stream": require.resolve("stream-browserify"),
        "util": require.resolve("util")
      })
      config.resolve.fallback = fallback;
      config.plugins = (config.plugins || []).concat([ 
        new webpack.ProvidePlugin({ 
         process: 'process/browser',
         Buffer: ['buffer', 'Buffer']
       }) 
      ]) 
      return config;
    },
  },
};
