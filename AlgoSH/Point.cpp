//
// Created by EM341 on 25/11/2025.
//

#include "Point.h"


Point::Point() {
    m_x = 0;
    m_y = 0;
}

Point::Point(int x, int y) {
    m_x = x;
    m_y = y;
}

int Point::X() {
    return m_x;
}
int Point::Y() {
    return m_y;
}
void Point::X(int x) {
    m_x = x;
}
void Point::Y(int y) {
    m_y = y;
}