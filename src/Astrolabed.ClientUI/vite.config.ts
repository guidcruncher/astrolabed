import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import Components from 'unplugin-vue-components/vite'

export default defineConfig({
  plugins: [
    vue(),
    Components({
      // Auto register components from these directories
      dirs: ['src/components', 'src/layouts', 'src/views'],
      extensions: ['vue'],
      deep: true,
      dts: 'src/components.d.ts' // Generates TypeScript declarations automatically
    })
  ],
  build: {
    // Output static assets directly to the .NET project's wwwroot directory
    outDir: path.resolve(__dirname, '../Astrolabed.Api/wwwroot'),
    emptyOutDir: true
  },
  server: {
    port: 8002,
    host: true
  }
})
