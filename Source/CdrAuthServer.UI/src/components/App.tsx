import Pages from '../pages/Index';
import { BrowserRouter as Router } from "react-router-dom";
import AppTheme from './AppTheme';
import { useState } from 'react';
import { CommonState } from '../@types/CommonContextType'
import { ConsentState } from '../@types/ConsentContextType'
import { LoginState } from '../@types/LoginContextType'
import { CommonContext } from '../context/CommonContext';
import { ConsentContext } from '../context/ConsentContext';
import { LoginContext } from '../context/LoginContext';

export default function App() {
  const [commonState, setCommonState] = useState<CommonState>({ pageTitle: "" });
  const [consentState, setConsentState] = useState<ConsentState>({});
  const [loginState, setLoginState] = useState<LoginState>({customerId:"",otp:""});

  return (
    <AppTheme>
      <Router>
        <CommonContext.Provider value={{ commonState, setCommonState }}>
          <ConsentContext.Provider value={{ consentState, setConsentState }}>
            <LoginContext.Provider value={{ loginState, setLoginState }}>
              <Pages />
            </LoginContext.Provider>
          </ConsentContext.Provider>
        </CommonContext.Provider>
      </Router>
    </AppTheme>
  );
}