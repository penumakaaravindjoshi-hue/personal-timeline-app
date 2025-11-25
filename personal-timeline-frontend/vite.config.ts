import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import basicSsl from '@vitejs/plugin-basic-ssl'; // <--- Importa el plugin
// https://vite.dev/config/
export default defineConfig({
    server: {
        port: 5173, // Asegúrate de que sea el puerto correcto
    },
    plugins: [
        basicSsl(), // <--- Agrega el plugin aquí
        tailwindcss(),
        react({
            babel: {
                plugins: [['babel-plugin-react-compiler']],
            },
        }),
    ],
});
