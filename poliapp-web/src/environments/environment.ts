// Producción: el frontend se publica en Azure Static Web Apps (https://www.poliapp.tech)
// y llama a la API que sigue en Azure App Service.
export const environment = {
  production: true,
  apiUrl: 'https://NOMBRE-APP.azurewebsites.net',
};
