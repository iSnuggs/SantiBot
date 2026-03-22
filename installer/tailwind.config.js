/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        santi: {
          500: "#0c95e9",
          600: "#0076c7",
          700: "#015ea1",
        },
      },
    },
  },
  plugins: [],
};
