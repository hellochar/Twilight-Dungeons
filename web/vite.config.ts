import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import checker from 'vite-plugin-checker'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), checker({ typescript: true })],
  base: process.env.VITE_BASE_PATH === '' ? './' : '/Twilight-Dungeons/',
  esbuild: {
    keepNames: true,
  },
  server: {
    host: true,
  },
})
