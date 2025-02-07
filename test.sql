WITH Step3Events AS (
  -- Select user_pseudo_id from step_3_deposit_success
  SELECT 
    user_pseudo_id,
    event_name,
    PARSE_DATE("%Y%m%d", event_date) AS event_date, 
    TIMESTAMP_MICROS(event_timestamp) AS event_ts,
    MAX(CASE WHEN params.key = "ga_session_id" THEN params.value.int_value ELSE NULL END) AS ga_session_id,
    REGEXP_REPLACE(
      CONCAT(
        CASE 
          WHEN traffic_source.source IS NULL OR traffic_source.source = '' 
          THEN '(direct)' 
          ELSE traffic_source.source 
        END,
        ' / ',
        CASE 
          WHEN traffic_source.medium IS NULL OR traffic_source.medium = '' 
          THEN '(none)' 
          ELSE traffic_source.medium 
        END
      ),
      r'^\((none|direct|google|[^\)]+)\)$',
      r'\1'
    ) AS source_medium,
    CASE 
      WHEN traffic_source.source IS NULL OR traffic_source.source = '' 
      THEN '(direct)' 
      ELSE traffic_source.source 
    END AS source,
    CASE 
      WHEN traffic_source.medium IS NULL OR traffic_source.medium = '' 
      THEN '(none)' 
      ELSE traffic_source.medium 
    END AS medium,
    MAX(CASE WHEN params.key = "campaign" THEN params.value.string_value ELSE NULL END) AS campaign,
    MAX(CASE WHEN params.key = "tx" THEN params.value.string_value ELSE NULL END) AS tx,
    MAX(CASE WHEN params.key = "from" THEN params.value.string_value ELSE NULL END) AS source_from,
    MAX(CASE 
        WHEN params.key = "payment_amount" 
        THEN COALESCE(params.value.double_value, params.value.float_value, params.value.int_value) 
        ELSE NULL 
    END) AS payment_amount,
    MAX(CASE 
        WHEN params.key = "value" 
        THEN COALESCE(params.value.double_value, params.value.float_value, params.value.int_value) 
        ELSE NULL 
    END) AS value
  FROM `analytics_455590991.events_*` AS events
  LEFT JOIN UNNEST(event_params) AS params
  WHERE event_name = "step_3_deposit_success"
  GROUP BY user_pseudo_id, event_name, event_date, event_ts, traffic_source
),

RankedSessions AS (
  -- Find the last 20 "session_start" events for users who performed step_3_deposit_success
  SELECT
    events.user_pseudo_id,
    PARSE_DATE("%Y%m%d", event_date) AS event_date,
    TIMESTAMP_MICROS(event_timestamp) AS event_ts,
    -- Split source_medium into source and medium
    CASE 
      WHEN REGEXP_EXTRACT(CONCAT(
          CASE 
            WHEN collected_traffic_source.manual_source IS NULL OR collected_traffic_source.manual_source = '' 
            THEN '(direct)' 
            ELSE collected_traffic_source.manual_source 
          END,
          ' / ',
          CASE 
            WHEN collected_traffic_source.manual_medium IS NULL OR collected_traffic_source.manual_medium = '' 
            THEN '(none)' 
            ELSE collected_traffic_source.manual_medium 
          END
        ), r'^(.*?) /') IS NULL 
      THEN '(direct)' 
      ELSE REGEXP_EXTRACT(CONCAT(
          CASE 
            WHEN collected_traffic_source.manual_source IS NULL OR collected_traffic_source.manual_source = '' 
            THEN '(direct)' 
            ELSE collected_traffic_source.manual_source 
          END,
          ' / ',
          CASE 
            WHEN collected_traffic_source.manual_medium IS NULL OR collected_traffic_source.manual_medium = '' 
            THEN '(none)' 
            ELSE collected_traffic_source.manual_medium 
          END
        ), r'^(.*?) /') 
    END AS source,
    CASE 
      WHEN REGEXP_EXTRACT(CONCAT(
          CASE 
            WHEN collected_traffic_source.manual_source IS NULL OR collected_traffic_source.manual_source = '' 
            THEN '(direct)' 
            ELSE collected_traffic_source.manual_source 
          END,
          ' / ',
          CASE 
            WHEN collected_traffic_source.manual_medium IS NULL OR collected_traffic_source.manual_medium = '' 
            THEN '(none)' 
            ELSE collected_traffic_source.manual_medium 
          END
        ), r' / (.*)$') IS NULL 
      THEN '(none)' 
      ELSE REGEXP_EXTRACT(CONCAT(
          CASE 
            WHEN collected_traffic_source.manual_source IS NULL OR collected_traffic_source.manual_source = '' 
            THEN '(direct)' 
            ELSE collected_traffic_source.manual_source 
          END,
          ' / ',
          CASE 
            WHEN collected_traffic_source.manual_medium IS NULL OR collected_traffic_source.manual_medium = '' 
            THEN '(none)' 
            ELSE collected_traffic_source.manual_medium 
          END
        ), r' / (.*)$') 
    END AS medium,
    ROW_NUMBER() OVER (PARTITION BY events.user_pseudo_id ORDER BY event_timestamp DESC) AS session_rank
  FROM `analytics_455590991.events_*` AS events
  LEFT JOIN UNNEST(event_params) AS params
  WHERE event_name = "session_start"
    AND events.user_pseudo_id IN (SELECT user_pseudo_id FROM Step3Events)
  GROUP BY events.user_pseudo_id, event_date, event_ts, event_timestamp, collected_traffic_source
),

ContributionCalc AS (
  -- Select source_medium for the last 20 sessions and filter for referral medium and specified sources
  SELECT 
    s3.user_pseudo_id,
    s3.tx AS tx,
    s3.event_date AS event_date,
    s3.source AS source,  -- Using rs.source here
    s3.value,
    s3.payment_amount,

    -- Check if any source matches 'referral' and the domain list
    MAX(CASE WHEN rs.medium = 'referral'  
        AND REGEXP_CONTAINS(LOWER(rs.source), r'^(coingabbar.com|coinchapter.com|crypto-reporter.com|coinedition.com|nftevening.com|themerkle.com|zycrypto.com|blockchainreporter.net|coinpedia.org|cryptowisser.com|bravenewcoin.com|blockonomi.com|techbullion.com|usethebitcoin.com|cryptwerk.com|financefeeds.com|theportugalnews.com|researchsnipers.com|analyticsinsight.net|kryptomoney.com|techktimes.com|otsnews.co.uk|thecoinrepublic.com|benzinga.com|mid-day.com|bignewsnetwork.com|researchsnipers.com|blockonomi.com|kryptomoney.com|financefeeds.com|coingabbar|bitnewsbot.com|usethebitcoin.com|analyticsinsight.net|cryptwerk.com|otsnews.co.uk|techbullion.com|theportugalnews.com|abcmoney.co.uk|harlemworldmagazine.com|streetinsider.com|moneyinc.com|theedinburghreporter.co.uk|coinchapter.com|coinmarketcal.com|cryptodaily.co.uk|thenewscrypto.com|timestabloid.com|newswatchtv.com|crypto-news-flash.com|crypto-reporter.com|decrypt.co|timestabloid.com|cryptoblogs.io|benzinga.com|themerkle.com|coindar.org|coincodecap.co|thecoinrepublic.com|financefeeds.com|marketwatch.com|dailybusinessgroup.co.uk)$') 
        THEN CONCAT(rs.source, ' / ', rs.medium) ELSE NULL END) AS source_medium

  FROM Step3Events s3
  LEFT JOIN RankedSessions rs
    ON s3.user_pseudo_id = rs.user_pseudo_id
  WHERE rs.session_rank <= 20  -- Only consider the last 20 sessions
  GROUP BY s3.user_pseudo_id, s3.tx, s3.event_date, s3.value, s3.source, s3.payment_amount
)

SELECT 
  tx AS transaction_id,
  event_date AS date,
  source_medium AS source_medium_name,  -- Full source/medium
  payment_amount AS deposit_amount
FROM ContributionCalc
WHERE source_medium IS NOT NULL