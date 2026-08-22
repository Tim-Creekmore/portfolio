/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{astro,html,js,jsx,md,mdx,ts,tsx}",
    "./public/assets/js/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: "#007BA7",
          dark: "#0d4f6e"
        }
      }
    }
  },
  plugins: []
};
