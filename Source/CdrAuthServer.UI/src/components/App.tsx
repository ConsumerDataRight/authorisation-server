import Pages from '../pages/Index';
import { BrowserRouter as Router } from "react-router-dom";
import AppTheme from './AppTheme';
import { RecoilRoot } from 'recoil';

export default function App() {

  return (
    <AppTheme>
      <RecoilRoot>
        <Router>
          <Pages />
        </Router>
      </RecoilRoot>
    </AppTheme>
  );
}