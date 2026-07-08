import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { nodePolyfills } from 'vite-plugin-node-polyfills';

export default defineConfig({
  envPrefix: ['VITE_', 'REACT_APP_'],
  base: '/ui/',

  define: {
    global: 'globalThis', // Fixed: Prevents global-scope definition crashes
  },

  plugins: [
    react(),

    nodePolyfills({
      include: ['crypto', 'stream', 'util', 'process', 'buffer'],
      globals: {
        Buffer: true,
        process: true,
      },
    }),
  ],

  build: {
    target: 'es2020',
    outDir: 'build',
  },

  resolve: {
    alias: {
      '@mui/styled-engine': '@mui/styled-engine-sc',
    },
  },

  optimizeDeps: {
    include: [
      '@mui/material',
      '@mui/icons-material',
      '@mui/styled-engine-sc',
      'react-router',
    ],
  },

  server: {
    port: 3000,
    open: true,
  },
});