
import { Route, Routes } from "react-router-dom";
import { AppAlert } from "../components/AppAlert";
import Confirmation from "./Confirmation";
import Login from "./Login";
import SelectAccount from "./SelectAccount";
import ArrangementDetails from "./Settings/ArrangementDetails";
import AccountList from "./Settings/AccountList";
import ScopeList from "./Settings/ScopeList";
import SharingPeriod from "./Settings/SharingPeriod";

export default function Pages() {
    return (
        <>
            <Routes>
                <Route path="/ui/login" element={<Login />} />
                <Route path="/ui/select-accounts" element={<SelectAccount />} />
                <Route path="/ui/confirmation" element={<Confirmation />} />
                <Route path="/ui/settings" element={<ArrangementDetails />} />
                <Route path="/ui/account-list" element={<AccountList />} />
                <Route path="/ui/data-requested" element={<ScopeList />} />
                <Route path="/ui/sharing-period" element={<SharingPeriod />} />
            </Routes>
            <AppAlert />
        </>
    );
}