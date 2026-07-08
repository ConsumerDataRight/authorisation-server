import * as React from 'react';
import { createRoot } from 'react-dom/client'; 
import CssBaseline from '@mui/material/CssBaseline';
import App from './components/App';

const container = document.getElementById('root');

if (!container) {
  throw new Error("Failed to find the root element");
}
const root = createRoot(container);

root.render(
  <React.Fragment>
    <CssBaseline />
    <App />
  </React.Fragment>
);
