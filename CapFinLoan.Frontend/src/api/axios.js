import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:7000',
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  console.log('[API Request]', {
    url: config.baseURL + config.url,
    method: config.method,
    hasToken: !!token,
    token: token ? `${token.substring(0, 20)}...` : 'none'
  });
  
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => {
    console.log('[API Response]', {
      url: response.config.url,
      status: response.status,
      data: response.data
    });
    return response;
  },
  (error) => {
    console.error('[API Error]', {
      url: error.config?.url,
      status: error.response?.status,
      message: error.response?.data?.message || error.message,
      data: error.response?.data
    });
    return Promise.reject(error);
  }
);

export default api;
