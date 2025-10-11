/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    './**/*.razor',
    './Components/**/*.{razor,cshtml,html}',
    './Pages/**/*.{razor,cshtml,html}'
  ],
  theme: {
    extend: {
      colors: {
        primary: '#1173d4',
        'background-light': '#f6f7f8',
        'background-dark': '#101922',
        'neutral-light': '#ffffff',
        'neutral-dark': '#1f2937',
        'text-light': '#101922',
        'text-dark': '#f6f7f8',
        'subtext-light': '#6b7280',
        'subtext-dark': '#9ca3af',
        'border-light': '#e5e7eb',
        'border-dark': '#374151',
        'content-light': '#ffffff',
        'content-dark': '#18232e',
        success: '#10b981',
        danger: '#ef4444',
      },
      borderRadius: {
        DEFAULT: '0.5rem',
        lg: '0.75rem',
        xl: '1rem',
        full: '9999px',
      },
      fontFamily: {
        display: ['Inter', 'sans-serif'],
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/container-queries'),
  ],
}
