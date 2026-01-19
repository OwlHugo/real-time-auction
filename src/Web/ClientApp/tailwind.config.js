
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  darkMode: 'media',
  theme: {
    extend: {
      colors: {
        primary: {
          DEFAULT: '#4F46E5',
          light: '#818CF8',
          dark: '#3730A3'
        },
        secondary: {
          DEFAULT: '#DB2777',
          light: '#F472B6',
          dark: '#9D174D'
        }
      },
      fontFamily: {
        sans: ['Inter', 'Roboto', 'sans-serif'],
      },
      animation: {
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      }
    },
  },
  plugins: [],
}
