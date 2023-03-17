import React from "react";
import { Redirect, Route, RouteComponentProps, RouteProps } from "react-router-dom";

export type ProtectedRouteProps = RouteProps & {
    isAuthenticated: boolean;
}

export class ProtectedRoute extends Route<ProtectedRouteProps> {
    render() {
        return (
            <Route render={(props: RouteComponentProps) => {
                if (!this.props.isAuthenticated) {
                    return <Redirect to='/ui/login' />
                }

                if (this.props.component) {
                    return React.createElement(this.props.component);
                }

                if (this.props.render) {
                    return this.props.render(props);
                }
            }} />
        );
    }
}
