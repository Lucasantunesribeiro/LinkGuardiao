/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        ink: 'rgb(var(--ink) / <alpha-value>)',
        sand: 'rgb(var(--sand) / <alpha-value>)',
        mist: 'rgb(var(--mist) / <alpha-value>)',
        sea: 'rgb(var(--sea) / <alpha-value>)',
        sun: 'rgb(var(--sun) / <alpha-value>)',
        clay: 'rgb(var(--clay) / <alpha-value>)',
        line: 'rgb(var(--line) / <alpha-value>)',
      },
      fontFamily: {
        sans: ['"Space Grotesk"', 'system-ui', 'sans-serif'],
        display: ['"Fraunces"', 'serif'],
      },
      boxShadow: {
        'soft-xl': '0 24px 48px -36px rgba(0, 0, 0, 0.45)',
      },
      animation: {
        rise: 'rise 0.6s ease-out',
        floaty: 'floaty 6s ease-in-out infinite',
      },
      keyframes: {
        rise: {
          '0%': { opacity: '0', transform: 'translateY(18px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        floaty: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-10px)' },
        },
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
  ],
}
