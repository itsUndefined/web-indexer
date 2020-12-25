import React                                     from 'react';
import { useStyles }                                     from './styles';
import { Button, IconButton, InputAdornment, TextField } from '@material-ui/core';
import { Search }                                        from '@material-ui/icons';

export const ResultsPage = () => {
    const styles = useStyles();

    const results = [{id: 1, title: 'Title1', abstract: 'Abstract1'}, {id: 2, title: 'Title2', abstract: 'Abstract2'}, {id: 3, title: 'Title2', abstract: 'Abstract3'}];

    const resultComponent = results.map((result) => (
        <div className={styles.result} key={result.id}>
            <div><a href={''}>{ result.title }</a></div>
            <div>
                Is the content relative?
                <select>
                    <option selected></option>
                    <option>Yes</option>
                    <option>No</option>
                </select>
            </div>
            <span style={{fontSize: '14px'}}>{ result.abstract }</span>
        </div>
    ));

    return (
        <div>
            <div className={styles.nav}>
                <div><a href={'/'} className={styles.logo}>My Search</a></div>
                <TextField id={'search'} style={{width: '500px', paddingLeft: '15px', paddingRight: '15px'}} fullWidth autoFocus placeholder={'Search bar'} autoComplete={'off'} variant={'outlined'} InputProps={{
                    endAdornment: (
                        <InputAdornment position={'end'}>
                            <IconButton edge={'end'}>
                                <Search/>
                            </IconButton>
                        </InputAdornment>
                    )
                }}>
                </TextField>
                <Button type={'submit'} variant={'contained'} color={'primary'}>
                    Submit your feedback
                </Button>
            </div>
            <div style={{paddingTop: '125px'}}>
                {resultComponent}
            </div>
        </div>
    );
};
