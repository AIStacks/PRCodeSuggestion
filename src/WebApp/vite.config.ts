import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { TanStackRouterVite } from "@tanstack/router-vite-plugin";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), TanStackRouterVite()],
  server: {
    host: "localhost",
    port: 18103,
    proxy: {
      "/CodeSuggestionHub": {
        target: "ws://localhost:18102",
        changeOrigin: true,
        ws: true,
      },
    },
  },
  build: {
    cssCodeSplit: true,
    rollupOptions: {
      // https://rollupjs.org/guide/en/#big-list-of-options
      output: {
        manualChunks: {
          bootstrap: ["bootstrap", "react-bootstrap"],
          formik: ["formik", "yup"],
          icon: ["react-icons"],
          vendor: ["react", "react-dom"],
          tanstack: ["@tanstack/react-router"],
        },
      },
    },
  },
});
