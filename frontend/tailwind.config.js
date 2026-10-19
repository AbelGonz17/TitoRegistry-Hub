/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          green: '#15803d',
          'green-light': '#22c55e',
          'green-hover': '#16a34a',
          black: '#0c110e',
          charcoal: '#121914',
          card: '#16201a',
          'card-hover': '#1b2820',
          'card-elevated': '#203127',
          border: 'rgba(34, 197, 94, 0.18)',
          'border-strong': 'rgba(34, 197, 94, 0.4)',
        },
        status: {
          completed: '#10b981',
          pending: '#0ea5e9',
          process: '#f59e0b',
          cancelled: '#ef4444',
        }
      },
      fontFamily: {
        body: ['Inter', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
        heading: ['Outfit', 'Inter', 'sans-serif'],
      },
      borderRadius: {
        'sm': '8px',
        'md': '14px',
        'lg': '20px',
      },
      boxShadow: {
        'card': '0 8px 30px rgba(0, 0, 0, 0.4)',
        'elevated': '0 14px 45px rgba(0, 0, 0, 0.6)',
        'glow': '0 0 25px rgba(34, 197, 94, 0.15)',
      }
    },
  },
  plugins: [],
};
