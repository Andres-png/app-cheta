import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      // Todo lo que empiece por /api va al backend .NET en 5174
      '/api': {
        target: 'http://localhost:5174',
        changeOrigin: true,
                secure: false,
      }
    }
  }
})