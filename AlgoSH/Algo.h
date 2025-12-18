#ifndef ALGOSH_ALGO_H
#define ALGOSH_ALGO_H
#include <vector>
#include "Point.h"

using namespace std;

class Algo {

    //Polygone à découper
    vector<Point> P;
    //Polygone fenêtre convexe, les points sont listé dans le sens horaire
    vector<Point> F;

    public:
    Algo(vector<Point> PL, vector<Point> PW);
    //retourne un booléen si S est visible par rapport à la droite (F1F2)
    bool visible(Point S, Point F1, Point F2);
    // retourne un booléen suivant l'intersection possible entre le côté [SP] du polygone et le bord prolongé (une droite) (F1F2) de la fenêtre
    bool coupe(Point S, Point P, Point F1, Point F2);
    // retourne le point d'intersection entre le segement [SP] et la droite (F1F2)
    Point intersection(Point S, Point P, Point F1, Point F2);
};


#endif //ALGOSH_ALGO_H