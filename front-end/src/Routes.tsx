import React                              from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';
import { SearchEnginePage }               from './pages/SearchEnginePage/SearchEnginePage';

export const MainRouterOutlet = () => {
    return (
        <Router>
            <Route path={'/'} component={SearchEnginePage}/>
        </Router>
    );
};