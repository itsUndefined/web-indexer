import React, { useState } from 'react';
import { IconButton, InputAdornment, TextField } from '@material-ui/core';
import { Search } from '@material-ui/icons';
import { useStyles } from './styles';
import { useHistory } from 'react-router-dom';


export const SearchEnginePage = () => {
    const styles = useStyles();

    const [query, setQuery] = useState<string>('');

    const history = useHistory();

    const doSearch = () => {
        // axios.get('http://localhost:5000/Documents', { params: { q: query }}).then(response => {
            // console.log(response);
            history.push(`/results?q=${query}`);
        // });
    };

    return (
        <div className={styles.search}>
            <div style={{width: '500px'}}>
                <div className={styles.logo}>My Search</div>
                <TextField 
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    id={'search'} 
                    fullWidth
                    autoFocus
                    placeholder={'Search bar'} 
                    autoComplete={'off'} 
                    variant={'outlined'}
                    onKeyPress={(e) => { if(e.key === 'Enter') doSearch() }}
                    InputProps={{
                        endAdornment: (
                            <InputAdornment position={'end'}>
                                <IconButton onClick={doSearch} edge={'end'}>
                                    <Search/>
                                </IconButton>
                            </InputAdornment>
                        )
                    }}
                >
                </TextField>
            </div>
        </div>
    );
};