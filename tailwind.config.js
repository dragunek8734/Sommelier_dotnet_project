/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        './**/*.{razor,html,cshtml}',
        './wwwroot/**/*.js',
    ],
    plugins: [
        require('@tailwindcss/forms'),
        require('@tailwindcss/aspect-ratio'),
        require('@tailwindcss/typography'),
    ],
    theme: {
        extend: {
            fontFamily: {
                'dancing-script': ['Dancing Script', 'cursive'],
            },
            colors: {
                'beige': {
                    50: '#faf6f1',  // Lightest beige, almost white
                    100: '#f5ede2', // Very light beige
                    200: '#ead9c7', // Light beige
                    300: '#e0c5ac', // Medium-light beige
                    400: '#d4b08e', // Medium beige
                    500: '#c49a73', // Medium-dark beige
                    600: '#b58158', // Dark beige
                    700: '#96694a', // Darker beige
                    800: '#7d573f', // Very dark beige
                    900: '#654636', // Darkest beige
                },
                'burgundy': {
                    50: '#f9f5f5',
                    100: '#f0e6e6',
                    200: '#e0cccc',
                    300: '#cca6a6',
                    400: '#b37e7e',
                    500: '#995c5c',
                    600: '#804d4d',
                    700: '#663e3e',
                    800: '#553535',
                    900: '#472d2d',
                },
                'brown': {
                    50: '#f8f6f4',
                    100: '#ebe5df',
                    200: '#d8c9bc',
                    300: '#c2a894',
                    400: '#ab866f',
                    500: '#8f6b55',
                    600: '#775647',
                    700: '#60453a',
                    800: '#503a31',
                    900: '#42312a',
                },
                'wine': {
                    50: '#f9f5f6',
                    100: '#f3e5e8',
                    200: '#e7c7cf',
                    300: '#d6a0ad',
                    400: '#c17585',
                    500: '#a84e61',
                    600: '#953a4d',
                    700: '#7a2f3f',
                    800: '#672835',
                    900: '#56242d'
                }
            }
        }
    }
}