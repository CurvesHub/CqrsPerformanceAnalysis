SELECT
    t.id AS run_id,
    t.api_to_use AS implementation,
    t.endpoint_name,
    t.seed,

    t.summary_content #>> '{metrics, http_reqs, count}' AS reqs_count,
    t.summary_content #>> '{metrics, http_reqs, rate}' AS reqs_rate_reqPerSec,

    t.summary_content #>> '{metrics, http_req_duration, min}' AS req_dur_min_ms,
    t.summary_content #>> '{metrics, http_req_duration, max}' AS req_dur_max_ms,
    t.summary_content #>> '{metrics, http_req_duration, avg}' AS req_dur_avg_ms,
    t.summary_content #>> '{metrics, http_req_duration, med}' AS req_dur_med_ms,
    t.summary_content #>> '{metrics, http_req_duration, p(90)}' AS req_dur_p_90_ms,
    t.summary_content #>> '{metrics, http_req_duration, p(95)}' AS req_dur_p_95_ms,

    t.summary_content #>> '{metrics, vus, min}' AS vus_min,
    t.summary_content #>> '{metrics, vus, max}' AS vus_max,

    t.summary_content #>> '{metrics, data_sent, rate}' AS data_sent_rate_bytesPerSec,
    t.summary_content #>> '{metrics, data_sent, count}' AS data_sent_count_bytes,
    t.summary_content #>> '{metrics, data_received, rate}' AS data_received_rate_bytesPerSec,
    t.summary_content #>> '{metrics, data_received, count}' AS data_received_count,

    t.summary_content #>> '{metrics, http_req_failed, value}' AS req_failed_percent,
    t.summary_content #>> '{metrics, http_req_failed, passes}' AS req_failed,
    t.summary_content #>> '{metrics, http_req_failed, fails}' AS req_passes
FROM
    test_runs t
