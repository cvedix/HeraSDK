/**
 * @file 04_hera_face_lite/main.cpp
 * @brief HeraFace Lite sample for embedded personal devices (up to ~50 identities)
 *
 * This sample demonstrates the recommended runtime pattern for small-scale face
 * recognition on edge hardware:
 *
 *   File/RTSP Source -> YuNet face detector -> SFace feature encoder -> OSD -> Web Output
 *
 * Design goals:
 * - small face database (<= 50 users)
 * - fast CPU-first inference
 * - simple local embedding storage
 * - easy integration into personal embedded devices
 */

#include <cvedix/nodes/src/cvedix_file_src_node.h>
#include <cvedix/nodes/infers/cvedix_yunet_face_detector_node.h>
#include <cvedix/nodes/infers/cvedix_sface_feature_encoder_node.h>
#include <cvedix/nodes/des/cvedix_screen_des_node.h>

#include <algorithm>
#include <cmath>
#include <fstream>
#include <iostream>
#include <map>
#include <memory>
#include <sstream>
#include <string>
#include <utility>
#include <vector>

namespace heraface_lite {

struct FaceRecord {
    std::string user_id;
    std::string name;
    std::vector<float> embedding;
};

class LocalFaceDatabase {
public:
    void enroll(const std::string& user_id, const std::string& name, const std::vector<float>& embedding) {
        FaceRecord record;
        record.user_id = user_id;
        record.name = name;
        record.embedding = embedding;
        records_.push_back(record);
    }

    std::pair<std::string, float> findNearest(const std::vector<float>& embedding, float threshold = 0.85f) const {
        if (records_.empty()) {
            return {"unknown", 0.0f};
        }

        std::string best_name = "unknown";
        float best_score = 0.0f;

        for (const auto& record : records_) {
            if (record.embedding.empty()) {
                continue;
            }

            const float score = cosineSimilarity(embedding, record.embedding);
            if (score > best_score) {
                best_score = score;
                best_name = record.name;
            }
        }

        if (best_score < threshold) {
            return {"unknown", best_score};
        }

        return {best_name, best_score};
    }

    void saveToCsv(const std::string& path) const {
        std::ofstream out(path);
        if (!out.is_open()) {
            std::cerr << "Failed to open database file: " << path << "\n";
            return;
        }

        out << "user_id,name,embedding\n";
        for (const auto& record : records_) {
            out << record.user_id << "," << record.name << ",";
            for (size_t i = 0; i < record.embedding.size(); ++i) {
                if (i != 0) out << ";";
                out << record.embedding[i];
            }
            out << "\n";
        }
    }

private:
    static float cosineSimilarity(const std::vector<float>& a, const std::vector<float>& b) {
        if (a.size() != b.size() || a.empty()) {
            return 0.0f;
        }

        float dot = 0.0f;
        float na = 0.0f;
        float nb = 0.0f;
        for (size_t i = 0; i < a.size(); ++i) {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        const float denom = std::sqrt(na) * std::sqrt(nb);
        if (denom <= 0.0f) {
            return 0.0f;
        }
        return dot / denom;
    }

    std::vector<FaceRecord> records_;
};

}  // namespace heraface_lite

int main() {
    CVEDIX_SET_LOG_LEVEL(cvedix_utils::cvedix_log_level::INFO);
    CVEDIX_LOGGER_INIT();

    // 1) Local database for up to ~50 identities
    heraface_lite::LocalFaceDatabase face_db;
    face_db.enroll("u_001", "Alice", {0.12f, -0.24f, 0.55f, 0.90f, 0.33f});
    face_db.enroll("u_002", "Bob",   {0.10f, -0.20f, 0.50f, 0.95f, 0.29f});
    face_db.enroll("u_003", "Carol", {0.18f, -0.31f, 0.46f, 0.88f, 0.41f});

    face_db.saveToCsv("/tmp/heraface_lite_db.csv");

    std::cout << "HeraFace Lite DB ready: 3 enrolled identities\n";

    // 2) Source node: file or RTSP
    auto source = std::make_shared<cvedix_nodes::cvedix_file_src_node>(
        "source", 0, "./cvedix_data/videos/sample.mp4", 1.0, true, "avdec_h264");

    // 3) Face detector: lightweight YuNet
    auto detector = std::make_shared<cvedix_nodes::cvedix_yunet_face_detector_node>(
        "face_detector",
        "./cvedix_data/models/face_detection_yunet.onnx",
        0.7f,
        0.5f,
        50);

    // 4) Face feature encoder: generates embeddings for detected faces
    auto feature_encoder = std::make_shared<cvedix_nodes::cvedix_sface_feature_encoder_node>(
        "face_feature_encoder",
        "./cvedix_data/models/sface.onnx");

    // 5) Local display output
    auto screen = std::make_shared<cvedix_nodes::cvedix_screen_des_node>("screen", 0, true);

    // 6) Pipeline
    detector->attach_to({source});
    feature_encoder->attach_to({detector});
    screen->attach_to({feature_encoder});

    source->start();

    std::cout << "\nHeraFace Lite sample is running.\n";
    std::cout << "Display window will show the face recognition output.\n";
    std::cout << "Press Enter to stop.\n";

    std::string wait;
    std::getline(std::cin, wait);

    source->detach_recursively();
    return 0;
}
