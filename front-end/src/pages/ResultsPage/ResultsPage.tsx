import React                                     from 'react';
import { useStyles }                                    from './styles';
import { IconButton, InputAdornment, TextField } from '@material-ui/core';
import { Search }                                       from '@material-ui/icons';

export const ResultsPage = () => {
    const styles = useStyles();

    const results = [{id: 1, title: 'Title1', abstract: 'Abstract1'}, {id: 2, title: 'Title2', abstract: 'Abstract2'}, {id: 3, title: 'Title2', abstract: 'Abstract3'}];

    const resultComponent = results.map((result) => (
        <div className={styles.result} key={result.id}>
            <div>{ result.title }</div>
            <span style={{fontSize: '14px'}}>{ result.abstract }</span>
        </div>
    ));

    return (
        <div className={styles.style}>
            <div className={styles.nav}>
                <div className={styles.logo}>My Search</div>
                <TextField id={'search'} style={{width: '500px', paddingLeft: '5px'}} fullWidth autoFocus placeholder={'Search bar'} autoComplete={'off'} variant={'outlined'} InputProps={{
                    endAdornment: (
                        <InputAdornment position={'end'}>
                            <IconButton edge={'end'}>
                                <Search/>
                            </IconButton>
                        </InputAdornment>
                    )
                }}>
                </TextField>
            </div>
            <div>
                {resultComponent}
            </div>
        </div>
    );
};
