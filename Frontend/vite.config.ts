import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { resolve } from 'path';
import { fileURLToPath } from 'url';


const __dirname = fileURLToPath(new URL('.', import.meta.url));

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': { target: 'http://localhost:5000', changeOrigin: true, secure: false }
    }
  },
  resolve: {
    alias: {},
  },
  css: {
    postcss: './postcss.config.js'
  },
  define: {
    __REACT_ROUTER_FUTURE_FLAGS__: JSON.stringify({
      v7_startTransition: true,
      v7_relativeSplatPath: true
    })
  }
})