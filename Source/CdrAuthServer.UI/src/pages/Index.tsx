
import { Route, Switch } from "react-router-dom";
import { AppAlert } from "../components/AppAlert";
import Confirmation from "./Confirmation";
// import { ProtectedRoute } from "../components/ProtectedRoute";
import Login from "./Login";
import SelectAccount from "./SelectAccount";
import ArrangementDetails from "./Settings/ArrangementDetails";
import AccountList from "./Settings/AccountList";
import ScopeList from "./Settings/ScopeList";
import SharingPeriod from "./Settings/SharingPeriod";

export default function Pages() {
    return (
        <>
            <Switch>
                <Route path="/ui/login" exact component={Login} />
                <Route path="/ui/select-accounts" exact component={SelectAccount} /> {/* TODO:C make it protected */}
                <Route path="/ui/confirmation" exact component={Confirmation} /> {/* TODO:C make it protected */}
                <Route path="/ui/settings" exact component={ArrangementDetails} /> {/* TODO:C make it protected */}
                <Route path="/ui/account-list" exact component={AccountList} /> {/* TODO:C make it protected */}
                <Route path="/ui/data-requested" exact component={ScopeList} /> {/* TODO:C make it protected */}
                <Route path="/ui/sharing-period" exact component={SharingPeriod} /> {/* TODO:C make it protected */}
            </Switch>
            <AppAlert />
        </>
    );
}