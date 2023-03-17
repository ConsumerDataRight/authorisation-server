export type BrandModel = {
    BrandId: string,
    BrandName: string,
    BrandAbn?: string,
}

export type CustomerModel = {
    LoginId: string,
    Accounts: AccountModel[]
}

export type AccountModel = {
    AccountId: string,
    DisplayName: string,
    ProductName?: string,
    MaskedName?: string,
    AccountNumber?:string
}

export type ClusterModel = {
    ScopeMatch: string,
    IncludeWhenDoesNotContain?:string,
    IncludeWhenContains?:string,
    DataCluster: string,
    Permissions: string
}

