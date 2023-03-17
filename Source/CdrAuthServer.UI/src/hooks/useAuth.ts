import settings from "../settings"
import jwt, { JwtPayload } from "jsonwebtoken";
import jwktopem from "jwk-to-pem";
import { JwksResponse } from "../models/Common";
import { CommonState, CommonStateModel, LoginParams } from "../state/Common.state";
import jose from "node-jose";
import { useRecoilState } from "recoil";

export function useAuth() {
    const [_, setCommonState] = useRecoilState(CommonState);

    const validateToken = async (token: string): Promise<LoginParams | null> => {
        try {
            const jwksUri = settings.JWKS_URI ?? "";
            if (jwksUri == "") {
                return null;
            }

            const decodedToken = jwt.decode(token, { complete: true });
            const kid = decodedToken?.header.kid;
            const loginParamsString = (decodedToken?.payload as JwtPayload)["login_params"];
            if (!loginParamsString) {
                return null;
            }
            const jwksResponse = await fetch(jwksUri).then((r) => {
                console.log(r);
                return r.json();
            }).catch((e) => {
                setCommonState((currentValue: CommonStateModel) => {
                    return {
                        ...currentValue,
                        errors: [{ title: "Token Validation Failed", code: "No JWKS", detail: "Failed to get JWKS from the Authentication Server." }]
                    } as CommonStateModel;
                });
                return null;
            });
            const jwksResponseModel = jwksResponse as JwksResponse;
            const matchingKeys = jwksResponseModel.keys.filter((k: any) => k.kid === kid);
            if (matchingKeys.length === 0) {
                return null;
            }

            var publicKey = jwktopem(matchingKeys[0]);

            const key = await jose.JWK.asKey(publicKey, 'pem');
            const verifier = jose.JWS.createVerify(key);
            const verified = await verifier.verify(token).catch((e) => {
                console.error(e);
            });
            const isVerified = !!verified;
            return isVerified == true ? JSON.parse(loginParamsString) as LoginParams : null;

        } catch (e) {
            return null;
        }
    }

    return {
        validateToken
    }
}