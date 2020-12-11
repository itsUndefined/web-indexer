import React from 'react';

export const SearchEnginePage = () => {
    return (
        <>
            <div style={{textAlign: 'center'}}>
                <div>Logo</div>
                <div>
                    <form>
                        <div>
                            Search
                            <select>
                                <option value={'web'} selected>the Web</option>
                            </select>
                            and Display the Results
                            <select>
                                <option value={'default'}>in Standard Form</option>
                            </select>
                        </div>
                        <div>
                            <input size={45} maxLength={200} />
                            <button type={'submit'}>Submit</button>
                        </div>
                    </form>
                </div>
            </div>
        </>
    );
};