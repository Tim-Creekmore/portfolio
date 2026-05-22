/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./index.html",
    "./*.html",
    "./portfolio/**/*.html",
    "./shop/**/*.html",
    "./content/**/*.html",
    "./game/index.html",
    "./assets/js/**/*.js"
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
