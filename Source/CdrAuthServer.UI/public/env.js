export const env = {
  // Fallback to Vite's build-time envs, but prioritize runtime window.env
  ...(import.meta.env || {}),
  ...(window?.env || {}), 
};