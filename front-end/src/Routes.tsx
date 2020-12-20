import React                              from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';
import { SearchEnginePage }               from './pages/SearchEnginePage/SearchEnginePage';
import { ResultsPage }                     from './pages/ResultsPage/ResultsPage';

export const MainRouterOutlet = () => {
    return (
        <Router>
            <Route path={'/'} exact component={SearchEnginePage}/>
            <Route path={'/results'} component={ResultsPage}/>
        </Router>
    );
};