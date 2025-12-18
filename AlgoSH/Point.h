//
// Created by EM341 on 25/11/2025.
//

#ifndef ALGOSH_POINT_H
#define ALGOSH_POINT_H


class Point {
    int m_x;
    int m_y;

    public:
    Point();
    Point(int x, int y);
    int X();
    int Y();
    void X(int x);
    void Y(int y);
};


#endif //ALGOSH_POINT_H