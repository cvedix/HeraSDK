package com.cvedix.sdk;

/** One detected/tracked object (immutable snapshot copied out of native memory). */
public final class Detection {
    public final int x;
    public final int y;
    public final int width;
    public final int height;
    public final int classId;
    public final float score;
    public final int trackId;
    public final String label;

    Detection(CvedixLibrary.Detection raw) {
        this.x = raw.x;
        this.y = raw.y;
        this.width = raw.width;
        this.height = raw.height;
        this.classId = raw.classId;
        this.score = raw.score;
        this.trackId = raw.trackId;
        this.label = raw.labelString();
    }

    @Override
    public String toString() {
        return String.format("%s(%.2f) track=%d [%d,%d %dx%d]",
                label, score, trackId, x, y, width, height);
    }
}
