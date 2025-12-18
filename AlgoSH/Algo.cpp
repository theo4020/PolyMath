#include "Algo.h"


Algo::Algo(vector<Point> PL, vector<Point> PW)
{
    P = PL;
    F = PW;
    Point S, f, I;

    for (int i = 0; i < f.size() - 1; i++) {
        vector<Point> PS;
        for (int j = 0; j < P.size(); j++) {
            if (j == 1) {
                f = P[j];
            }
            else if (coupe(S, P[j], F[i], F[i+1])) {
                I = intersection(S, P[j], F[i], F[i+1]);
                PS.push_back(I);
            }
            S = P[j];
            if (visible(S, F[i], F[i+1])) {
                PS.push_back(S);
            }
        }

        if (PS.size() > 0) {
            if (coupe(S, f, F[i], F[i+1])) {
                I = intersection(S, f, F[i], F[i+1]);
                PS.push_back(I);
            }
            P = PS;
        }
    }
}

//retourne un booléen si S est visible par rapport à la droite (F1F2)
bool Algo::visible(Point S, Point F1, Point F2) {
    //Vecteur F1F2
    Point F1F2(F2.X() - F1.X(), F2.Y() - F1.Y());
    //Vecteur F1S
    Point F1S(S.X() - F1.X(), S.Y() - F1.Y());
    return (F1S.X() * F1F2.Y() - F1S.Y() * F1F2.X()) > 0;
}

// retourne un booléen suivant l'intersection possible entre le côté [SP] du polygone et le bord prolongé (une droite) (F1F2) de la fenêtre
bool Algo::coupe(Point S, Point P, Point F1, Point F2) {
    return visible(S, F1, F2) && visible(P, F1, F2);
}

// retourne le point d'intersection entre le segement [SP] et la droite (F1F2)
Point Algo::intersection(Point P1, Point P2, Point P3, Point P4) {
    //Matrice A
    int a = P2.X() - P1.X(), b = P3.X() - P4.X();
    int c = P2.Y() - P1.Y(), d = P3.Y() - P4.Y();
    //
    int detA = a*d - b*c;
    if (detA == 0) {
        return P1;
    }
    //Matrice A^-1
    vector<vector<int>> X{
        { (1/detA) * d , (1/detA) * (-b) },
        { (1/detA) * (-c) , (1/detA) * a }
    };
    int BX = P3.X() - P1(X);
    int BY = P3.Y() - P1(Y);
    int t = X[O][0] * BX + X[O][1] * BY;
    Point Result( P1.X() + (P2.X() * t) - (P1.X() * t) , P1.Y() + (P2.Y() * t) - (P1.Y() * t) );
    return Result;

}
